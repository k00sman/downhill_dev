using UnityEditor;

public class MeshImportSettings : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        ModelImporter importer = (ModelImporter)assetImporter;

        importer.animationType = ModelImporterAnimationType.None;
        importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
    }
}