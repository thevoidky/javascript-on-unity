let state = null;

const printState = () => {
    log(`JS state name: ${state.name}; level: ${state.level}`);
};

window.setGameState = (stateString) => {
    state = JSON.parse(stateString);
    printState();
};
