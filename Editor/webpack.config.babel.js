import entry from './entry';
import output from './output';

module.exports = env => {
    return {
        entry: entry,
        module: {
            rules: [
                { test: /\.js$/, loader: 'babel-loader' }
            ]
        },
        output: output,
        optimization: {
            minimize: env != 'dev'
        }
    };
};
