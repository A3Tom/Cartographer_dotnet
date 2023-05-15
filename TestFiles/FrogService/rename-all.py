import os

path = "C:\TestFiles\FrogService\ALBS.PondService"

from_name = "ALBS.FrogService."
to_name = "ALBS.PondService."

def rename_dirs_and_files(path):
    for root, dirs, files in os.walk(path):
        for dirname in dirs:
            if dirname.startswith(from_name):
                old_dirname = os.path.join(root, dirname)
                new_dirname = os.path.join(root, dirname.replace(
                    from_name, to_name))
                os.rename(old_dirname, new_dirname)
        for filename in files:
            if filename.startswith(from_name):
                old_filename = os.path.join(root, filename)
                new_filename = os.path.join(root, filename.replace(
                    from_name, to_name))
                os.rename(old_filename, new_filename)


rename_dirs_and_files(path)
