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
