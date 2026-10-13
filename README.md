# PlexAnilListSync

Expected tautulli json data value
`grandparent_guid` to get the `plex://show` guid and not the `plex://episode` guid

```json
{
  "showTitle": "{show_name}",
  "season": "{season_num}",
  "episode": "{episode_num}",
  "action": "{action}",
  "plexGuid": "{grandparent_guid}",
  "episodeRatingKey": "{rating_key}",
  "seasonRatingKey": "{parent_rating_key}",
  "showRatingKey": "{grandparent_rating_key}",
  "type": "{media_type}",
  "user": "{user}"
}
```

## Notifications

The app can send push notifications through [ntfy](https://docs.ntfy.sh/). Set `Notifications:Enabled` to `true` to turn the feature on. It is off by default.

Each AniList user can have a `Notifications` block. The app then sends "Watched episode X of {Media}" to that user's topic when it updates AniList. For a movie it sends "Watched {Media}". Errors go to the `Notifications:Errors` topic. `ErrorThrottle` sets the wait time between equal errors. The default is 10 minutes.

The `AccessToken` is optional. The app sends it as a bearer token. Put secrets in the mounted `config/appsettings.json`, not in the image.

This will also send an error when the anilist token expires, so you don't have to remember to update it yearly.

```json
"Notifications": {
  "Enabled": true,
  "Errors": { "TopicUrl": "https://ntfy.example.com/errors", "AccessToken": "tk_..." },
  "ErrorThrottle": "00:10:00"
},
"Anilist": {
  "Users": [
    {
      "Token": "...",
      "PlexUsernames": ["myFirstUser"],
      "Notifications": { "TopicUrl": "https://ntfy.example.com/myFirstUser", "AccessToken": "tk_..." }
    }
  ]
}
```
