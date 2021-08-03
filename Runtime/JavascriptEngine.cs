using System;
using System.Collections.Generic;
using Jint;
using Jint.Runtime.Interop;
using UnityEngine;

namespace Modules.Runtime
{
    public abstract class JavascriptEngine
    {
        private readonly Engine _engine;
        private readonly HashSet<string> _typeNames = new HashSet<string>();

        private bool _isInitialized;

        protected JavascriptEngine()
        {
            _engine = new Engine();
            _engine.SetValue("window", this);
        }

        public abstract ISet<Type> TypesToBind { get; }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            var types = TypesToBind;
            var typeofMonoBehaviour = typeof(MonoBehaviour);
            var typeofJavascriptEngine = typeof(JavascriptEngine);
            foreach (var type in types)
            {
                if (!type.IsClass || type.IsSubclassOf(typeofMonoBehaviour) ||
                    type.IsSubclassOf(typeofJavascriptEngine))
                {
                    continue;
                }

                if (_typeNames.Contains(type.Name))
                {
                    Debug.LogError($"{GetType()}: This type is exist already. ({type.Name})");
                    continue;
                }

                _engine.SetValue(type.Name, TypeReference.CreateTypeReference(_engine, type));
                _typeNames.Add(type.Name);
            }

            _isInitialized = true;
        }

        public void Run(string source)
        {
            Initialize();

            _engine.Execute(source);
        }
    }
}