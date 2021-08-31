import {SampleClass} from './.SampleClass.js';

class __class_SampleEngine {
constructor() {}
SetBoolean(boolean_value) { return False; }
GetBoolean() { return False; }
SetInteger(int32_value) { return 0; }
GetInteger() { return 0; }
SetString(string_value) { return new String(); }
GetString() { return new String(); }
Log(Object_message) {}
LogThreeTimesJsAsync(string_first,string_second,string_third) { return new Promise(null); }
BooleanProp = False;
IntegerProp = 0;
StringProp = '';
}

export const window = new __class_SampleEngine();
