import entry from './entry.json';

module.exports = env => {
    return {
        entry: entry,
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
