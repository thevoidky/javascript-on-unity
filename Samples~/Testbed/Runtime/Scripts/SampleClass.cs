using System;
using Cysharp.Threading.Tasks;
using Jint.Native;
using OOTL.JavascriptOnUnity.Runtime;
using UnityEngine;

namespace Testbed.Runtime.Scripts
{
    public class SampleClass : BoundClass
    {
        // Bound property
        public string Name { get; set; }

        // Bound method, see .SampleEngine.js and sample2.js
        public void Say(string script) => Debug.Log($"{Name}: {script}");

        // Bound method returning a Promise (awaitable), see .SampleClass.js and sample2.js
        public JsValue MoveJsAsync(float relativeX, float relativeY, float relativeZ)
        {
            return Promise(Handle, this);

            // Invoked function directory
            void Handle(Delegate resolve, SampleClass c) => Act(c).ContinueWith(resolve.Resolve).Forget();

            // Actual function
            async UniTask Act(SampleClass c) => await c.MoveAsync(new Vector3(relativeX, relativeY, relativeZ));
        }

        public async UniTask MoveAsync(Vector3 relative)
        {
            var target = _transform.position + relative;
            while (true)
            {
                var position = _transform.position;
                var dist = target - position;
                var dir = dist.normalized;

                var mov = dir * Time.deltaTime * _speed;
                var arrived = mov.sqrMagnitude >= dist.sqrMagnitude;
                _transform.position = arrived ? target : position + mov;

                if (arrived)
                {
                    break;
                }

                await UniTask.WaitForEndOfFrame();
            }
        }

        public SampleClass(JavascriptEngine jsEngine, string name) : base(jsEngine)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _transform = cube.transform;

            Name = name;
        }

        private Transform _transform;

        private float _speed = 5f;
    }
}