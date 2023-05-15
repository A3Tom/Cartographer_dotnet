import json

# Set the input and output file names
input_file_name = "internalServices.json"
output_file_name = "output.mmd"

# Read the JSON file into a Python list of dictionaries
with open(input_file_name, "r") as input_file:
    data = json.load(input_file)

# Generate the MermaidJS code as a string
mmd_code = "graph LR;\n\nFrog[\"fa:fa-frog\"] --> FrogService\n"
for item in data:
    for service in item["InternalServices"]:
        mmd_code += f"{item['ServiceId']}[\"{item['FriendlyName']}\"] --> {service['ServiceId']}[\"{service['FriendlyName']}\"]\n"
mmd_code += "\n"

# Write the MermaidJS code to a text file
with open(output_file_name, "w") as output_file:
    output_file.write(mmd_code)

print(f"MermaidJS code written to {output_file_name}")
