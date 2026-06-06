#!/usr/bin/env bash
# Build the mod and produce a UMM-ready release zip.
#
# Output: EitRForWotr-v<version>.zip in the repo root, structured as:
#   EitRForWotr/
#     EitRForWotr.dll
#     BlueprintCore.dll
#     Info.json
#     Localization/*.json
#
# Drop the zip into UnityModManager's "install mod" prompt, or extract the
# inner EitRForWotr/ folder into <game>/Mods/.
#
# Side effect: `dotnet build` also deploys to a local WOTR install for
# development convenience (controlled by $WrathDir / $ModDir in the csproj).

set -euo pipefail
cd "$(dirname "$0")"

ID="EitRForWotr"
VERSION=$(python3 -c "import json; print(json.load(open('Info.json'))['Version'])")
ZIP_NAME="${ID}-v${VERSION}.zip"

rm -f "$ZIP_NAME"
dotnet build -c Release --nologo

STAGE=$(mktemp -d)
trap 'rm -rf "$STAGE"' EXIT

mkdir -p "$STAGE/$ID/Localization"
cp "bin/Release/net472/$ID.dll" "$STAGE/$ID/"
cp Info.json     "$STAGE/$ID/"
cp Localization/*.json "$STAGE/$ID/Localization/"

# Bundle BlueprintCore.dll (UMM probes the mod folder for dependencies).
BC_DLL=$(find "$HOME/.nuget/packages/ww-blueprint-core" -path "*/lib/net472/BlueprintCore.dll" 2>/dev/null | sort -V | tail -1)
if [ -z "$BC_DLL" ]; then
  echo "error: BlueprintCore.dll not found in ~/.nuget/packages/ww-blueprint-core/." >&2
  echo "       run 'dotnet restore' to populate the NuGet cache, then retry." >&2
  exit 1
fi
cp "$BC_DLL" "$STAGE/$ID/"

# zip is preferred over `tar -a` here so Windows users can extract it natively.
(cd "$STAGE" && zip -qr "$OLDPWD/$ZIP_NAME" "$ID")

echo
echo "Built: $ZIP_NAME"
unzip -l "$ZIP_NAME"
