using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Parser;
using Jint.Parser.Ast;
using Jint.Runtime.Interop;
using UnityEngine;

namespace Modules.Runtime
{
    public abstract class JavascriptEngine
    {
        private readonly Engine _engine = new Engine();
        private readonly JavaScriptParser _parser = new JavaScriptParser();
        private readonly HashSet<string> _typeNames = new HashSet<string>();
        private readonly Dictionary<string, Program> _programs = new Dictionary<string, Program>();

        private bool _isInitialized;

        protected JavascriptEngine()
        {
            static void SetTimeout(Delegate callback, int intervalMilliseconds) =>
                TimeoutRoutine(callback, intervalMilliseconds);

            static void TimeoutRoutine(Delegate c, int ms) => UniTask.Delay(ms)
                .ContinueWith(() => c.DynamicInvoke(JsValue.Undefined, new[] {JsValue.Undefined}));

            // 즈언통의 window를 사용
            _engine.SetValue("window", this);

            // setTimeout을 만들어줘야 다른 Promise들도 작동한다
            _engine.SetValue("setTimeout", new Action<Delegate, int>(SetTimeout));

            // TODO: 코드를 미리 파싱해서 빠르게 실행할 수 있도록, 아래는 샘플 코드
            // var program = _parser.Parse(source);
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