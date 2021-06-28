const wait = (milliseconds) => new Promise(resolve => {
    setTimeout(() => resolve(), milliseconds);
});

const asyncFunction = async () => {
    setText('This is a text');
    await wait(2000);
    setText('And now it\'s changed after wait');
};

asyncFunction();
