import os
import re

view_models = []
models_dir = 'src/Aiursoft.Translate/Models'
for root, dirs, files in os.walk(models_dir):
    for file in files:
        if file.endswith('ViewModel.cs'):
            view_models.append(os.path.join(root, file))

for vm_path in view_models:
    with open(vm_path, 'r') as f:
        content = f.read()
    
    # Check if class inherits from UiStackLayoutViewModel
    class_match = re.search(r'public class (\w+)\s*:\s*UiStackLayoutViewModel', content)
    class_name_match = re.search(r'public class (\w+)', content)
    
    if class_name_match:
        class_name = class_name_match.group(1)
        inherits = class_match is not None
        has_constructor = re.search(r'public\s+' + class_name + r'\s*\(', content)
        has_page_title = 'PageTitle =' in content or 'PageTitle=' in content
        print(f"{vm_path}: inherits={inherits}, constructor={has_constructor is not None}, page_title={has_page_title}")

