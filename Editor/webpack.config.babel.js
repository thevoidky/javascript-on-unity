module.exports = env => {
    return {
        entry: require('./entry.json'),
        module: {
            rules: [
                {
                    test: /\.(?:js|ts)$/,
                    loader: 'babel-loader',
                    exclude: /node_modules/,
                },
            ],
        },
        resolve: {
            extensions: ['.ts', '.js'],
        },
        output: require('./output.json'),
        optimization: {
            minimize: env != 'dev'
        },
    };
};
