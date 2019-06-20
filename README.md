# Building

Easiest to use a Docker image (running from project directory in Powershell here):

```powershell
docker run --rm -it -p 4000:4000 -v ${PWD}:/app -w /app etelej/jekyll
```

Note the following problems and limitations, however:

* The above Docker image does not pass `--watch` onto Jekyll, so it must be restarted to review each change. I should fork and tweak at some point.
* The `baseurl` in `_config.yml` should be set to `""` for local testing
* For some reason I don't yet understand, I must remove all `apple-touch-icon*` files from the root directory for the Jekyll to compile my site. Just remember not to commit :) Need to return to this later.