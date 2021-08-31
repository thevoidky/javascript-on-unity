using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Jint.Native;
using OOTL.JavascriptOnUnity.Runtime;
using UnityEngine;

namespace Testbed.Runtime.Scripts
{
    public class SampleEngine : JavascriptEngine
    {
        // Implement this property to bind classes(not structs) you want,
        // except MonoBehaviour and JavascriptEngine(and derived them also)
        public override ISet<Type> TypesToBind => new HashSet<Type>() { typeof(SampleClass), typeof(Vector3) };

        // Bound properties
        public bool BooleanProp { get; set; }
        public int IntegerProp { get; set; }
        public string StringProp { get; set; }

        // Bound methods
        public bool SetBoolean(bool value) => BooleanProp = value;
        public bool GetBoolean() => BooleanProp;
        public int SetInteger(int value) => IntegerProp = value;
        public int GetInteger() => IntegerProp;
        public string SetString(string value) => StringProp = value;
        public string GetString() => StringProp;
        public void Log(object message) => Debug.Log(message);

        // Bound async method, see .SampleEngine.js and sample.js
        // If you append suffix "JsAsync", the method of helper javascript returns Promise
        public JsValue LogThreeTimesJsAsync(string first, string second, string third)
        {
            return Promise(Handle);

            // Invoked function directory
            void Handle(Delegate resolve)
            {
                Act().ContinueWith(resolve.Resolve).Forget();
            }

            // Actual function
            async UniTask Act()
            {
                Debug.Log(first);
                await UniTask.Delay(500);
                Debug.Log(second);
                await UniTask.Delay(500);
                Debug.Log(third);
            }
        }
    }
}