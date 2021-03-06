module.exports = env => {
    return {
        entry: {
            // runner: './runner.js',
            // runnerWithException: './runnerWithException.js',
            runnerES6: './runnerES6.js',
            runnerWithGlobalVariableES6: './runnerWithGlobalVariableES6',
            runnerWithGlobalFunctionES6: './runnerWithGlobalFunctionES6',
            runnerSavingGameES6: './runnerSavingGameES6',
            runnerLoadingGameES6: './runnerLoadingGameES6',
            runnerTimeoutES6: './runnerTimeoutES6',
            runnerTimeoutWithPromiseES6: './runnerTimeoutWithPromiseES6'
        },
        module: {
            rules: [
                { test: /\.js$/, loader: 'babel-loader' }
            ]
        },
        optimization: {
            minimize: env != 'dev'
        }
    };
};
