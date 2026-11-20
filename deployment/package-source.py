"""Package deployment source without development configuration or build artifacts."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parent.parent
output = root / "obj" / "buildwise-source.zip"
output.parent.mkdir(exist_ok=True)
excluded_dirs = {".git", ".vs", ".codex", ".agents", ".aws", "bin", "obj", "tests", "node_modules", "__pycache__"}
excluded_suffixes = {".map", ".user", ".suo", ".pptx", ".pdb", ".pfx", ".pem", ".env"}
excluded_names = {"appsettings.Development.json", "Scafold.txt", "secrets.json"}
with ZipFile(output, "w", ZIP_DEFLATED) as archive:
    for path in root.rglob("*"):
        relative = path.relative_to(root)
        if (path.is_file() and not path.is_symlink() and not excluded_dirs.intersection(relative.parts)
                and path.suffix not in excluded_suffixes and path.name not in excluded_names
                and not path.name.startswith((".env", "secrets."))
                and not (path.name.startswith("appsettings.") and path.name != "appsettings.json")):
            archive.write(path, relative.as_posix())
print(output)
