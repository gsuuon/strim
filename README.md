# 📹strim

Become a Twitch strimmer without clicking buttons

https://user-images.githubusercontent.com/6422188/233827259-bf5189d5-808d-4037-9450-db6526971462.mp4

- 🤳 watch yourself
- 👆 no buttons (protect your fingy)


## Use
Make sure you have a recent `ffmpeg` available and you're on Windows.

1. Set `TWITCH_STREAM_KEY` to your [stream key](https://dashboard.twitch.tv/settings/stream) 
2. `strim`
3. `Ctrl-c` to stop

- You can pass an [ingest url](https://stream.twitch.tv/ingests/) as the first argument: `strim rtmp://scl01.contribute.live-video.net/app/`

### From repo
```
dotnet run
```

### From nuget
```
dotnet tool install --global Gsuuon.Tool.Strim
strim
```

