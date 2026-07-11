# SQL Server pe Kubernetes

Completare/demonstratie de Kubernetes pentru tema, **alaturi de**
`docker/` (Docker Compose), nu in locul lui - workflow-ul zilnic de
dezvoltare ramane Docker Compose (mai simplu, un singur `docker compose up`).
Ca si la Docker, **doar SQL Server** ruleaza in cluster; aplicatia desktop
Avalonia ramane locala si se conecteaza la baza de date fie prin
port-forward, fie prin NodePort-ul expus (vezi sectiunea 5).

## 0. Prerechizite

- `kubectl` instalat.
- Un cluster local: [minikube](https://minikube.sigs.k8s.io/docs/start/)
  sau [kind](https://kind.sigs.k8s.io/docs/user/quick-start/). Exemplele de
  mai jos folosesc minikube, dar orice cluster local cu suport pentru
  `NodePort` si `PersistentVolumeClaim` (storage class implicita) merge la
  fel de bine.

```bash
minikube start
```

## 1. Fisierele din acest folder

| Fisier | Rol |
|---|---|
| `namespace.yaml` | Namespace-ul dedicat `restaurant-datanase` |
| `secret.yaml` | **Template comis in git**, cu placeholder-uri - nu se aplica direct |
| `secret.local.yaml.example` | Ghid de copiat in `secret.local.yaml` (gitignored) |
| `persistent-volume-claim.yaml` | PVC pentru `/var/opt/mssql` (5Gi, ReadWriteOnce) |
| `deployment.yaml` | Deployment SQL Server 2022, 1 replica, probes, resources |
| `service.yaml` | Service NodePort (port 1433, nodePort 30143) |
| `configmap-schema.yaml` | Copie a `database/schema.sql` + `docker/create-app-user.sql` |
| `init-job.yaml` | Job care ruleaza schema + creeaza userul "marius", o singura data |

## 2. Genereaza `k8s/secret.local.yaml`

```bash
cp k8s/secret.local.yaml.example k8s/secret.local.yaml
```

Genereaza baza64 pentru cele doua parole (acelasi `MSSQL_SA_PASSWORD`/
`DB_USER_PASSWORD` ca in `docker/.env`, daca vrei parolele identice intre
Docker si K8s - nu e obligatoriu sa fie aceleasi):

```bash
echo -n 'ParolaTaSaReala123!' | base64
echo -n 'ParolaTaDbUserReala456!' | base64
```

Deschide `k8s/secret.local.yaml` si inlocuieste cele doua placeholder-uri
(`<base64-ul parolei ... aici>`) cu valorile generate mai sus.
`k8s/secret.local.yaml` **nu** se comite in git (e in `.gitignore`).

## 3. Aplica manifestele, in ordine

`k8s/init-job.yaml` asteapta singur (bucla de retry interna, pana la 2.5
minute) pana cand Service-ul raspunde, deci ordinea exacta dintre
Deployment/Service/Job nu e critica - dar Namespace-ul si Secret-ul trebuie
sa existe **inainte** de orice altceva (celelalte resurse le refera):

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secret.local.yaml
kubectl apply -f k8s/persistent-volume-claim.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/configmap-schema.yaml
kubectl apply -f k8s/init-job.yaml
```

(Poti aplica si tot folderul deodata cu `kubectl apply -f k8s/ --recursive`,
insa `kubectl` ar incerca sa aplice si `secret.yaml` - template-ul cu
placeholder-uri, care va esua intentionat. Fie sterge/muti local
`secret.yaml` din folder inainte, fie tine-te de comenzile individuale de
mai sus.)

Daca modifici `database/schema.sql` sau `docker/create-app-user.sql`
ulterior, regenereaza `configmap-schema.yaml` inainte de a-l re-aplica -
comanda exacta e in comentariul de la inceputul fisierului.

## 4. Verificare

```bash
kubectl get pods -n restaurant-datanase
```

Ar trebui sa vezi un pod `sqlserver-...` `Running`/`1/1 Ready` si, dupa ce
Job-ul termina, un pod `sqlserver-init-...` `Completed`. Urmareste Job-ul in
timp real:

```bash
kubectl logs -n restaurant-datanase job/sqlserver-init -f
```

Ultima linie ar trebui sa fie "Schema si userul aplicatiei au fost create
cu succes." Daca Job-ul a esuat (`kubectl get jobs -n restaurant-datanase`
arata 0/1), verifica intai ca pod-ul `sqlserver` e `Ready`
(`kubectl describe pod -n restaurant-datanase -l app=sqlserver` - de regula
parola gresita in Secret sau resurse insuficiente pe cluster sunt cauza).

Test rapid de conexiune, din interiorul clusterului:

```bash
kubectl run sqlcmd-test --rm -it --restart=Never -n restaurant-datanase \
  --image=mcr.microsoft.com/mssql/server:2022-latest -- \
  /opt/mssql-tools18/bin/sqlcmd -S sqlserver -U marius -P '<parola-marius>' -C \
  -d RestaurantDataNase -Q "SELECT name FROM sys.tables;"
```

## 5. Conectarea aplicatiei locale la baza din K8s

Doua variante, la alegere:

**a) NodePort** (mai simplu, fara un proces separat de mentinut) - portul
`30143` e expus deja pe IP-ul nodului:

```bash
minikube ip   # IP-ul nodului minikube
```

Connection string (similar celui din `RestaurantDataNaseGUI/appsettings.Development.json`
pentru Docker Compose, doar cu host/port diferite):

```
Server=<minikube-ip>,30143;Database=RestaurantDataNase;User Id=marius;Password=<parola-marius>;TrustServerCertificate=True;
```

**b) port-forward** (util daca clusterul nu expune usor IP-ul nodului, ex.
Docker driver pe minikube):

```bash
kubectl port-forward -n restaurant-datanase svc/sqlserver 14331:1433
```

Connection string:

```
Server=localhost,14331;Database=RestaurantDataNase;User Id=marius;Password=<parola-marius>;TrustServerCertificate=True;
```

(Port `14331` ales deliberat diferit de `14330`, portul folosit de
containerul Docker Compose - ca sa poti avea ambele variante pornite
simultan fara conflict, daca vrei sa le compari.)

## 6. Stergere / reset

Sterge tot ce tine de acest namespace (Deployment, Service, PVC, Secret,
ConfigMap, Job - tot):

```bash
kubectl delete namespace restaurant-datanase
```

Aceasta sterge si PVC-ul, deci si datele (echivalentul `docker compose down
-v`). Pentru un restart "curat", repeta pasii 3-4 de la zero (Job-ul va
recrea schema si userul).

Daca vrei doar sa opresti temporar fara sa pierzi datele (analog `docker
compose down`, fara `-v`), scaleaza Deployment-ul la 0 replici in loc sa
stergi namespace-ul intreg:

```bash
kubectl scale deployment/sqlserver -n restaurant-datanase --replicas=0
# ... si inapoi la 1 cand vrei sa repornesti:
kubectl scale deployment/sqlserver -n restaurant-datanase --replicas=1
```
