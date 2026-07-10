import os

def remove_diacritics(text):
    mapping = {
        'ă': 'a', 'â': 'a', 'î': 'i', 'ș': 's', 'ț': 't',
        'Ă': 'A', 'Â': 'A', 'Î': 'I', 'Ș': 'S', 'Ț': 'T'
    }
    for char, replacement in mapping.items():
        text = text.replace(char, replacement)
    return text

def process_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    new_content = remove_diacritics(content)
    
    if new_content != content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f"Processed: {file_path}")

def main():
    root_dir = "."
    for root, dirs, files in os.walk(root_dir):
        if 'bin' in dirs: dirs.remove('bin')
        if 'obj' in dirs: dirs.remove('obj')
        
        for file in files:
            if file.endswith('.cs') or file.endswith('.axaml'):
                process_file(os.path.join(root, file))

if __name__ == "__main__":
    main()
