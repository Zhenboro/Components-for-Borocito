"""
Crea un archivo .zip por cada proyecto que tenga un .exe dentro de bin/Debug.

El ZIP contiene TODO el contenido de la carpeta bin/Debug,
incluyendo subcarpetas, DLL, EXE, archivos de configuración, etc.

La carpeta Debug NO se incluye dentro del ZIP.
"""

import os
import zipfile

# Ruta base donde se encuentran los proyectos
base_path = os.getcwd()

compressed_folder = "binarios"
compress_ext = ".zip"

# Ruta de la carpeta binarios donde se almacenarán los ZIP
binarios_path = os.path.join(base_path, compressed_folder)
os.makedirs(binarios_path, exist_ok=True)


# Recorremos las carpetas de los proyectos dentro de la solución
for project in os.listdir(base_path):

    project_path = os.path.join(base_path, project)

    # Ignoramos todo lo que no sean carpetas
    if not os.path.isdir(project_path):
        continue

    # Ruta a bin/Debug
    bin_debug_path = os.path.join(project_path, "bin", "Debug")

    # Verificamos que exista la carpeta bin/Debug
    if not os.path.isdir(bin_debug_path):
        continue

    # Buscamos los .exe directamente dentro de Debug
    exe_files = [
        file
        for file in os.listdir(bin_debug_path)
        if file.lower().endswith(".exe")
    ]

    # Si no hay ningún .exe, no creamos ZIP para este proyecto
    if not exe_files:
        continue

    # Usamos el nombre del primer EXE para nombrar el ZIP
    exe_name = exe_files[0]
    zip_name = os.path.splitext(exe_name)[0] + compress_ext
    zip_path = os.path.join(binarios_path, zip_name)

    # Creamos el ZIP
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zipf:

        # Recorremos TODO el contenido de Debug de forma recursiva
        for root, dirs, files in os.walk(bin_debug_path):

            for file in files:

                file_path = os.path.join(root, file)

                # Ruta relativa respecto a Debug.
                # Esto evita que aparezca "Debug/" dentro del ZIP.
                arcname = os.path.relpath(
                    file_path,
                    bin_debug_path
                )

                zipf.write(file_path, arcname=arcname)

    print(f"Creado archivo ZIP: {zip_path}")


print("¡Empaquetado completado!")
