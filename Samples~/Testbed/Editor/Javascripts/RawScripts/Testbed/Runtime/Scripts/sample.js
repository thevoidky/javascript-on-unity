import {window} from "./.SampleEngine";
import {SampleClass} from "./.SampleClass";

const asyncFunction = async () => {
    // window.PrivateFunction('error!');    // It doesn't work, this script will be terminated
    let robot = new SampleClass(window, 'robot');
    while (true) {
        await robot.MoveJsAsync(2, 0, 0);
        robot.Say("right");
        await robot.MoveJsAsync(-4, 0, 0);
        robot.Say("left");
        await robot.MoveJsAsync(2, 0, 0);
    }
};

asyncFunction();
