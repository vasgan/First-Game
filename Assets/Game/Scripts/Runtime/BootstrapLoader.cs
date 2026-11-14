using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstGame
{
    public sealed class BootstrapLoader : MonoBehaviour
    {
        private void Start() => SceneManager.LoadScene("Level01");
    }
}
