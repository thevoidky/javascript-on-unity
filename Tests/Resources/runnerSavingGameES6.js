let state = {
    name: "Alice",
    level: 2,
};

window.getGameState = () => {
    return JSON.stringify(state);
};
