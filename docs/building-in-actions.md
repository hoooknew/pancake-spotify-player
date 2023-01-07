# Building in Actions

https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows#running-your-workflow-only-when-a-push-of-specific-tags-occurs

https://stackoverflow.com/questions/63932728/github-action-release-tag

https://github.com/actions-ecosystem/action-regex-match

https://blog.harshcasper.com/debugging-github-actions-workflows-effectively
https://github.com/nektos/act
https://www.docker.com/pricing/

https://github.com/microsoft/github-actions-for-desktop-apps/blob/main/.github/workflows/cd.yml

``` yaml
on:
  push:
    tags:
      - '*'
```

``` yaml
# Create the release:  https://github.com/actions/create-release
- name: Create release
    id: create_release
    uses: actions/create-release@v1
    env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }} # This token is provided by Actions, you do not need to create your own token
    with:
        tag_name: ${{ github.ref}}.${{matrix.ChannelName}}.${{ matrix.targetplatform }}
        release_name:  ${{ github.ref }}.${{ matrix.ChannelName }}.${{ matrix.targetplatform }}
        draft: false
        prerelease: false
```


https://docs.github.com/en/actions/using-workflows/reusing-workflows
https://docs.github.com/en/actions/using-workflows/storing-workflow-data-as-artifacts
https://github.com/actions/upload-artifact
https://github.com/actions/download-artifact