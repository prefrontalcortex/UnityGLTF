using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
	internal static class MaterialEditorBridge
	{
		[InitializeOnLoadMethod]
		private static void ConnectGltfExporterToPbrGraphGUI()
		{
			PBRGraphGUI.ImmutableMaterialChanged += OnImmutableMaterialChanged;
		}

		private static void OnImmutableMaterialChanged(Material material)
		{
			if (!material) return;
			if (!AssetDatabase.Contains(material)) return;

			var assetPath = AssetDatabase.GetAssetPath(material);

			// TODO handle case where mainAsset is a GameObject; we can still write materials back in that case
			// var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);

			// Transform[] rootTransforms = null;
			var glftSettingsCopy =  ScriptableObject.Instantiate(GLTFSettings.GetOrCreateSettings());
			glftSettingsCopy.ExportFullPath = true;
			glftSettingsCopy.TryExportTexturesFromDisk = true;
			var exportSettings = new ExportOptions(glftSettingsCopy);
			exportSettings.TexturePathRetriever += texture =>
			{
				return AssetDatabase.GetAssetPath(texture);
			};
			var exporter = new GLTFSceneExporter((Transform[]) null, exportSettings);
			// load all materials from mainAsset
			var importer = AssetImporter.GetAtPath(assetPath) as GLTFImporter;
			if (!importer) return;

			var path = Path.GetDirectoryName(assetPath);
			var name = Path.GetFileName(assetPath);



			// var allObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			foreach (var obj in importer.m_Materials)
			{
				if (!(obj is Material mat))
				{
					// TODO warn that there are extra objects we can't store right now
					continue;
				}
				exporter.ExportMaterial(mat);
			}

			var images = exporter.GetRoot().Images;
			foreach (var image in images)
				if (!string.IsNullOrEmpty(image.Uri))
					image.Uri  = Path.GetRelativePath(  path, image.Uri);

			// Save file and make sure we reimport it
			exporter.SaveGLTFandBin(path, name, false);
			ScriptableObject.DestroyImmediate(glftSettingsCopy);
			AssetDatabase.ImportAsset(assetPath);

			importer = AssetImporter.GetAtPath(assetPath) as GLTFImporter;
			if (!importer) return;


			// add texture remaps, so that the textures we just exported with stay valid.
			// We want to always have default paths in the file, and remapping is done on the Unity side to a project specific path,
			// to avoid breaking references all the time when paths change.
			var exportedTextures = exporter.GetRoot().Textures;

			for (int i = 0; i < exportedTextures.Count; i++)
			{
				var exported = exportedTextures[i];
				var sourceTextureForExportedTexture = exporter.GetSourceTextureForExportedTexture(exported);
				importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Texture2D), exported.Name), sourceTextureForExportedTexture);
			}
			importer.SaveAndReimport();

			// TODO we should get rid of this, but without it currently the inspector doesn't repaint
			// after importing a changed material, which can be confusing. Could be caching inside PBRGraphGUI
			AssetDatabase.Refresh();

			EditorApplication.update += () =>
			{
				// Repaint Inspector, newly imported values can be different if we're not perfectly round tripping
				foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors)
				{
					editor.Repaint();
				}
			};
		}
	}
}
