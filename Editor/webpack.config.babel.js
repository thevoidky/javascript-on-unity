import entry from './entry';
import output from './output';

module.exports = env => {
    return {
        entry: entry,
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
        output: output,
        optimization: {
            minimize: env != 'dev'
        }
    };
};
