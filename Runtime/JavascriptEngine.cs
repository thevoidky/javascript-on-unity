using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Parser;
using Jint.Parser.Ast;
using Jint.Runtime.Interop;
using UnityEngine;

namespace OOTL.JavascriptOnUnity.Runtime
{
    public abstract class JavascriptEngine
    {
        private Engine _engine = new Engine();
        private readonly HashSet<string> _typeNames = new HashSet<string>();

        private readonly JavaScriptParser _parser = new JavaScriptParser();
        private readonly Dictionary<string, Program> _programs = new Dictionary<string, Program>();

        private JsValue _newPromise;

        private bool _isInitialized;

        private static void SetTimeout(Delegate callback, int milliseconds) =>
            UniTask.Delay(milliseconds).ContinueWith(callback.Resolve);

        internal Engine Engine => _engine;

        public abstract ISet<Type> TypesToBind { get; }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            // 즈언통의 window를 사용
            var typename = GetType().Name;
            Engine.SetValue(typename, this);
            // Engine.SetValue("window", this);

            // setTimeout을 만들어줘야 다른 Promise들도 작동한다
            Engine.SetValue("setTimeout", new Action<Delegate, int>(SetTimeout));

            Engine.Execute(@"
const __createPromise__ = function (action, p1, p2, p3, p4, p5, p6, p7, p8) {
    return new Promise(function(resolve) {
        action(resolve, p1, p2, p3, p4, p5, p6, p7, p8);
    });
};"
            );

            _newPromise = Engine.GetValue("__createPromise__");
            var types = TypesToBind;
            var typeofMonoBehaviour = typeof(MonoBehaviour);
            var typeofJsEngine = typeof(JavascriptEngine);
            foreach (var type in types)
            {
                if (!type.IsClass || type.IsSubclassOf(typeofMonoBehaviour) || type.IsSubclassOf(typeofJsEngine))
                {
                    continue;
                }

                if (_typeNames.Contains(type.Name))
                {
                    Debug.LogWarning($"{GetType()}: This type is exist already, will be skipped. ({type.Name})");
                    continue;
                }

                Engine.SetValue(type.Name, TypeReference.CreateTypeReference(Engine, type));
                _typeNames.Add(type.Name);
            }

            _isInitialized = true;
        }

        public void RunDirectly(string source)
        {
            Initialize();
            Engine.Execute(_parser.Parse(source));
        }

        public JsValue RunDirectlyAndGetValue(string source)
        {
            Initialize();
            return Engine.Execute(_parser.Parse(source)).GetCompletionValue();
        }

        public void Run(string key)
        {
            if (!_programs.ContainsKey(key))
            {
                Debug.LogError($"{GetType()}: Not be compiled as this key. ({key})");
                return;
            }

            Run(_programs[key]);
        }

        public bool Compile(string key, string source)
        {
            try
            {
                var program = _parser.Parse(source);
                if (_programs.ContainsKey(key))
                {
                    Debug.LogWarning($"{GetType()}: Contains key already, it will be overridden. ({key})");
                }

                _programs[key] = program;

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        public void Remove(string key) => _programs.Remove(key);

        public void Reset()
        {
            _engine = new Engine();
            _programs.Clear();
            _typeNames.Clear();

            _isInitialized = false;
            Initialize();
        }

        private void Run(Program program)
        {
            Initialize();

            Engine.Execute(program);
        }

        protected JsValue Promise(Action<Delegate> handle) => _newPromise.Invoke(JsValue.FromObject(Engine, handle));

        protected JsValue Promise<T>(Action<Delegate, T> handle, T parameter) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, parameter));

        protected JsValue Promise<T1, T2>(
            Action<Delegate, T1, T2> handle,
            T1 p1, T2 p2) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2));

        protected JsValue Promise<T1, T2, T3>(
            Action<Delegate, T1, T2, T3> handle,
            T1 p1, T2 p2, T3 p3) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3));

        protected JsValue Promise<T1, T2, T3, T4>(
            Action<Delegate, T1, T2, T3, T4> handle,
            T1 p1, T2 p2, T3 p3, T4 p4) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4));

        protected JsValue Promise<T1, T2, T3, T4, T5>(
            Action<Delegate, T1, T2, T3, T4, T5> handle,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4), JsValue.FromObject(Engine, p5));

        protected JsValue Promise<T1, T2, T3, T4, T5, T6>(
            Action<Delegate, T1, T2, T3, T4, T5, T6> handle,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4), JsValue.FromObject(Engine, p5), JsValue.FromObject(Engine, p6));

        protected JsValue Promise<T1, T2, T3, T4, T5, T6, T7>(
            Action<Delegate, T1, T2, T3, T4, T5, T6, T7> handle,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4), JsValue.FromObject(Engine, p5), JsValue.FromObject(Engine, p6),
                JsValue.FromObject(Engine, p7));

        protected JsValue Promise<T1, T2, T3, T4, T5, T6, T7, T8>(
            Action<Delegate, T1, T2, T3, T4, T5, T6, T7, T8> handle,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4), JsValue.FromObject(Engine, p5), JsValue.FromObject(Engine, p6),
                JsValue.FromObject(Engine, p7), JsValue.FromObject(Engine, p8));
    }

    public abstract class BoundClass
    {
        private readonly JavascriptEngine _jsEngine;
        private readonly JsValue _newPromise;

        private Engine Engine => _jsEngine.Engine;

        protected BoundClass(JavascriptEngine jsEngine)
        {
            _jsEngine = jsEngine;
            _newPromise = Engine.GetValue("__createPromise__");
        }

        protected JsValue Promise(Action<Delegate> handle) => _newPromise.Invoke(JsValue.FromObject(Engine, handle));

        protected JsValue Promise<T>(Action<Delegate, T> handle, T parameter) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, parameter));

        protected JsValue Promise<T1, T2>(
            Action<Delegate, T1, T2> handle,
            T1 p1, T2 p2) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2));

        protected JsValue Promise<T1, T2, T3>(
            Action<Delegate, T1, T2, T3> handle,
            T1 p1, T2 p2, T3 p3) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3));

        protected JsValue Promise<T1, T2, T3, T4>(
            Action<Delegate, T1, T2, T3, T4> handle,
            T1 p1, T2 p2, T3 p3, T4 p4) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4));

        protected JsValue Promise<T1, T2, T3, T4, T5>(
            Action<Delegate, T1, T2, T3, T4, T5> handle,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4), JsValue.FromObject(Engine, p5));

        protected JsValue Promise<T1, T2, T3, T4, T5, T6>(
            Action<Delegate, T1, T2, T3, T4, T5, T6> handle,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4), JsValue.FromObject(Engine, p5), JsValue.FromObject(Engine, p6));

        protected JsValue Promise<T1, T2, T3, T4, T5, T6, T7>(
            Action<Delegate, T1, T2, T3, T4, T5, T6, T7> handle,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4), JsValue.FromObject(Engine, p5), JsValue.FromObject(Engine, p6),
                JsValue.FromObject(Engine, p7));

        protected JsValue Promise<T1, T2, T3, T4, T5, T6, T7, T8>(
            Action<Delegate, T1, T2, T3, T4, T5, T6, T7, T8> handle,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8) =>
            _newPromise.Invoke(
                JsValue.FromObject(Engine, handle),
                JsValue.FromObject(Engine, p1), JsValue.FromObject(Engine, p2), JsValue.FromObject(Engine, p3),
                JsValue.FromObject(Engine, p4), JsValue.FromObject(Engine, p5), JsValue.FromObject(Engine, p6),
                JsValue.FromObject(Engine, p7), JsValue.FromObject(Engine, p8));
    }

    public static class ResolveExtension
    {
        public static void Resolve(this Delegate resolve) =>
            resolve?.DynamicInvoke(JsValue.Undefined, new[] {JsValue.Undefined});
    }
}