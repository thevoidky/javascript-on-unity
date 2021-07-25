import entry from '../../../../Assets/Javascript on Unity/Editor/entry';
import output from '../../../../Assets/Javascript on Unity/Editor/output';

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
