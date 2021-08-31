using UnityEngine;

namespace Testbed.Runtime.Scripts
{
    public class Sample : MonoBehaviour
    {
        [SerializeField]
        private TextAsset js;

        private SampleEngine _engine;

        private void Awake()
        {
            _engine = new SampleEngine();
            _engine.Compile("js", js.text);

            _engine.Run("js");
        }
    }
}