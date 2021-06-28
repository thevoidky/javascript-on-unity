using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Jint.Runtime.Interop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace Modules.Tests.Editor.Scripts
{
    public class TestRunner
    {
        [Test]
        public void Log()
        {
            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Debug.Log));
            engine.Execute(@"log('Test to Debug.Log()');");
        }

        [Test]
        public void RunFunction()
        {
            static string Foo(int num) => $"C# can see that you passed: {num}";

            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Debug.Log));
            engine.SetValue("func", new Func<int, string>(Foo));
            engine.Execute(@"
let integer = 1010;
let responseFromCsharp = func(integer);
log ('Response from C#: ' + responseFromCsharp);
");
        }

        private class TestApi
        {
            public void ApiMethod1() => Debug.Log("Called api method 1");

            public int ApiMethod2()
            {
                Debug.Log("Called api method 2");
                return 2;
            }
        }

        [Test]
        public void RunMethod()
        {
            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Debug.Log));
            engine.SetValue("ApiRunner", TypeReference.CreateTypeReference(engine, typeof(TestApi)));
            engine.Execute(@"
let apiRunner = new ApiRunner();
apiRunner.ApiMethod1();
let result = apiRunner.ApiMethod2();
log('api method 2 has returned ' + result);
");
        }

        private class WorldModel
        {
            public string PlayerName { get; set; } = "Alice";
            public int NumberOfDonuts { get; set; } = 2;

            public void Message() => Debug.Log("This is a function");
        }

        [Test(Description = "Capture object on Javascript side, that has been created on C# side")]
        public void RunCapturingObject()
        {
            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Debug.Log));

            var world = new WorldModel();
            engine.SetValue("world", world);
            Debug.Log($"{world.PlayerName} has {world.NumberOfDonuts} donuts");

            engine.Execute(@"
log('Javascript can see that ' + world.PlayerName + ' has ' + world.NumberOfDonuts + ' donuts');
world.Message();
world.NumberOfDonuts += 3;
log(world.PlayerName + ' has now ' + world.NumberOfDonuts + ' donuts. Thanks, JavaScript, for giving us some');
");

            Debug.Log(
                $"{world.PlayerName} has now {world.NumberOfDonuts} donuts. Thanks, JavaScript, for giving us some");
        }

        [Test(Description =
            "Capture object on Javascript side, that has been created on C# side and in another block scope")]
        public void RunCapturingObjectFromOutside()
        {
            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Debug.Log));

            {
                var world = new WorldModel();
                engine.SetValue("world", world);
            }

            engine.Execute(@"
log('Javascript can see that ' + world.PlayerName + ' has ' + world.NumberOfDonuts + ' donuts');
world.Message();
world.NumberOfDonuts += 3;
log(world.PlayerName + ' has now ' + world.NumberOfDonuts + ' donuts. Thanks, JavaScript, for giving us some');
");
        }

        [Test]
        public void RunJsFile()
        {
            var textAsset = Resources.Load<TextAsset>("runner");
            Assert.IsNotNull(textAsset);

            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Debug.Log));
            engine.Execute(textAsset.text);

            engine.Execute("hello();");
            var result = engine.GetCompletionValue().AsString();
            Debug.Log($"C# got function result from Javascript: {result}");
        }

        [Test]
        public void RunWithException()
        {
            var textAsset = Resources.Load<TextAsset>("runnerWithException");
            Assert.IsNotNull(textAsset);

            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Debug.Log));

            try
            {
                engine.Execute(textAsset.text);
                Debug.LogError($"This script must throw JavaScriptException!");
            }
            catch (JavaScriptException e)
            {
                var location = engine.GetLastSyntaxNode().Location.Start;
                var error =
                    $"Jint runtime error {e.Error} (Line {location.Line}, Column {location.Column})\n{PrintBody(textAsset.text)}";

                Debug.Log(error);
            }

            static string PrintBody(string body)
            {
                if (string.IsNullOrEmpty(body)) return "";
                var lines = body.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
                return string.Join("\n", Enumerable.Range(0, lines.Length).Select(i => $"{i + 1:D3} {lines[i]}"));
            }
        }

        [Test]
        public void RunES6()
        {
            var textAsset = Resources.Load<TextAsset>("dist/runnerES6");
            Assert.IsNotNull(textAsset);

            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Debug.Log));
            engine.Execute(textAsset.text);
        }

        [Test]
        public void RunWebpackAndGlobalVariableES6()
        {
            var textAsset = Resources.Load<TextAsset>("dist/runnerWithGlobalVariableES6");
            Assert.IsNotNull(textAsset);

            var engine = new Engine();
            engine.Execute("let window = this;");
            engine.SetValue("log", new Action<object>(Debug.Log));
            engine.Execute(textAsset.text);
        }

        [Test]
        public void RunWithGlobalFunctionES6()
        {
            var textAsset = Resources.Load<TextAsset>("dist/runnerWithGlobalFunctionES6");
            Assert.IsNotNull(textAsset);

            var engine = new Engine();
            engine.Execute("let window = this;");
            engine.Execute(textAsset.text);
            engine.Execute("hello();");

            Debug.Log($"C# got result from function: {engine.GetCompletionValue()}");
        }

        [Test]
        public void RunSavingStateES6()
        {
            var textAsset = Resources.Load<TextAsset>("dist/runnerSavingGameES6");
            Assert.IsNotNull(textAsset);

            var engine = new Engine();
            engine.Execute("let window = this");
            engine.Execute(textAsset.text);
            engine.Execute("getGameState();");
            Debug.Log($"Game State: {engine.GetCompletionValue().AsString()}");
        }

        [Test]
        public void RunLoadingStateES6()
        {
            var jsAsset = Resources.Load<TextAsset>("dist/runnerLoadingGameES6");
            Assert.IsNotNull(jsAsset);

            var stateAsset = Resources.Load<TextAsset>("savedstate");
            Assert.IsNotNull(stateAsset);

            var engine = new Engine();
            engine.SetValue("log", new Action<object>(Debug.Log));
            engine.Execute("let window = this;");
            engine.Execute(jsAsset.text);
            engine.Invoke("setGameState", stateAsset.text);
        }

        [UnityTest]
        public IEnumerator RunTimeoutES6()
        {
            var textAsset = Resources.Load<TextAsset>("dist/runnerTimeoutES6");
            Assert.IsNotNull(textAsset);

            var labelText = string.Empty;

            var engine = new Engine();
            engine.SetValue("setText", new Action<string>(text =>
            {
                Debug.Log($"setText - labelText({text})");
                labelText = text;
            }));
            engine.Execute("let window = this");

            engine.SetValue("setTimeout", new Action<Delegate, int>((callback, interval) =>
            {
                async UniTask Routine(Delegate callback, int intervalMilliseconds)
                {
                    Debug.Log($"Routine - {intervalMilliseconds}");
                    try
                    {
                        await UniTask.Delay(intervalMilliseconds);
                        callback.DynamicInvoke(JsValue.Undefined, new[] {JsValue.Undefined});
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }

                Routine(callback, interval).Forget();
            }));

            engine.Execute(textAsset.text);

            var oldValue = labelText;
            Debug.Log($"labelText - {labelText}");

            yield return new WaitUntil(() => oldValue != labelText);
        }

        [UnityTest]
        public IEnumerator RunTimeoutWithPromiseES6()
        {
            var textAsset = Resources.Load<TextAsset>("dist/runnerTimeoutWithPromiseES6");
            Assert.IsNotNull(textAsset);

            var labelText = string.Empty;

            var engine = new Engine();
            engine.SetValue("setText", new Action<string>(text =>
            {
                Debug.Log($"setText - labelText({text})");
                labelText = text;
            }));
            engine.Execute("let window = this");
            engine.SetValue("log", new Action<object>(Debug.Log));

            engine.SetValue("setTimeout", new Action<Delegate, int>((callback, interval) =>
            {
                Debug.Log($"start setTimeout - {interval}");

                async UniTask Routine(Delegate callback, int intervalMilliseconds)
                {
                    Debug.Log($"Routine - {intervalMilliseconds}");

                    try
                    {
                        await UniTask.Delay(intervalMilliseconds);
                        callback.DynamicInvoke(JsValue.Undefined, new[] {JsValue.Undefined});
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }

                Routine(callback, interval).Forget();
            }));

            engine.Execute(textAsset.text);

            var oldValue = labelText;
            Debug.Log($"labelText - {labelText}");

            yield return new WaitUntil(() => oldValue != labelText);
        }
    }
}