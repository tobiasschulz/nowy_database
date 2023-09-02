const fs = require('fs');
const path = require('path');
const glob = require('glob');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');
const {globSync} = require("glob");

console.log(path.resolve(process.cwd(), 'wwwroot/output'));

module.exports = function (env, {mode}) {
  const production = mode === 'production';

  if (fs.existsSync('webpack.config.hook.js')) {
    const {hook} = require(path.resolve(process.cwd(), 'webpack.config.hook'));
    hook();
  }

  let entry_points = {};

  fs.mkdirSync(path.resolve(process.cwd(), 'resources/bundles/'), {recursive: true});
  fs.writeFileSync(path.resolve(process.cwd(), 'resources/bundles/.keep'), new Uint8Array());

  for (let file_path of glob.globSync(path.resolve(process.cwd(), `resources/bundles/*.ts`))) {
    let entry_name = path.parse(file_path).name;
    entry_points[entry_name] ??= [];
    entry_points[entry_name].push(file_path);
  }
  for (let file_path of glob.globSync(path.resolve(process.cwd(), `resources/bundles/*.scss`))) {
    let entry_name = path.parse(file_path).name;
    entry_points[entry_name] ??= [];
    entry_points[entry_name].push(file_path);
  }

  console.log(entry_points);

  return {

    node: {
      global: false,
      __filename: false,
      __dirname: false,
    },

    mode: production ? 'production' : 'development',
    devtool: production ? 'source-map' : 'inline-source-map',
    entry: entry_points,

    output: {
      path: path.resolve(__dirname, 'wwwroot/output'),
      filename: '[name].js',
      clean: true,
      module: true,
      library: {
        type: "module",
      },
    },

    experiments: {
      outputModule: true,
    },

    resolve: {
      extensions: ['.ts', '.js'],
      modules: ['resources', 'node_modules'],
      plugins: [new TsconfigPathsPlugin({
        configFile: path.resolve(__dirname, 'tsconfig.webpack.json'),
      })]
    },

    module: {
      rules: [
        {
          test: /\.s[ac]ss$/i,
          use: [
            // Creates `style` nodes from JS strings
            MiniCssExtractPlugin.loader, // production ? MiniCssExtractPlugin.loader : "style-loader",
            // Translates CSS into CommonJS
            {
              loader: 'css-loader',
              options: {
                url: true,
                sourceMap: true,
              }
            },
            {
              loader: 'resolve-url-loader',
              options: {
                sourceMap: true,
              }
            },
            // Compiles Sass to CSS
            {
              loader: 'sass-loader',
              options: {
                sourceMap: true, // <-- !!IMPORTANT!!
              }
            },
          ],
        },
        {
          test: /\.ts$/i,
          use: [
            {
              loader: 'ts-loader',
              options: {
                configFile: "tsconfig.webpack.json"
              }
            }
          ],
          exclude: /node_modules/
        }
      ]
    },

    plugins: [
      new MiniCssExtractPlugin(),
    ],

  }
};
