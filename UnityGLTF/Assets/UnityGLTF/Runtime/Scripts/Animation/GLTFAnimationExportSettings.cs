using UnityEngine;

namespace UnityGLTF
{
	public class GLTFAnimationExportSettings : MonoBehaviour
	{
		[Tooltip("If enabled animator states with the same name " +
		         "or clips that have the same name will be merged into one animation clip on export")]
		public bool mergeClipsWithMatchingNames = true;
	}
}
