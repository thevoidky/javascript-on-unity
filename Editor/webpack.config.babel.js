module.exports = env => {
    return {
        entry: require('./entry.json'),
        module: {
            rules: [
                {
                    test: /\.js$/,
                    loader: 'babel-loader',
                    exclude: /node_modules/,
                },
            ],
        },
        resolve: {
            extensions: ['.js'],
        },
        output: require('./output.json'),
        optimization: {
            minimize: env != 'dev'
        },
    };
};
