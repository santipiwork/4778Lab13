# Weather World — Lab 13

Open `Assets/Scenes/WeatherLab.unity` in Unity 6000.2.8f1 and press Play. The left panel selects Orlando, London, Dubai, Tokyo, or Sydney. Six physical billboards display three internet images, with each image reused on a second billboard.

## Weather

The editor reads the local OpenWeather key from `UserSettings/WeatherLabCredentials.json`. This directory is excluded from Git and is not included in builds. In a standalone build, enter the key in the game panel for the current session. Do not put a key in source code or commit it in a scene.

Turn off Preview to fetch current weather. The request uses HTTPS, JSON, and metric units. City changes refresh immediately; automatic refresh runs every ten minutes. HTTP 401 means the key was rejected; a newly created key may need a few hours to activate. Preview uses explicitly labeled fictional weather, fixed example UTC offsets, and 06:00–18:00 daylight. Live data supplies the actual city offset, sunrise, and sunset.

Skybox selection handles day, night, clear, overcast/fog, rain/thunderstorm/drizzle, snow, sunrise, and sunset. The directional sun changes intensity, color, and angle. Night and twilight take priority over weather. Light color also varies with temperature.

The imported Fantasy Skybox FREE pack is connected to the weather scene using the FS003 panoramic set. The eight slots are Day, Night, Day (Sunny), Day_Sunless, Rainy, Snowy, Sunrise, and Sunset. The pack uses Day for clear sunny weather. Both the scene's initial sky and the runtime weather slots reference the imported materials. `Weather Lab > Assign imported Fantasy skyboxes` restores this mapping, and Create scene uses it automatically when the pack is present. The old `Preview_*.mat` files remain as fallback assets.

Allow Downloads over HTTP is enabled as requested by the assignment, although the implemented URLs use HTTPS.

## Image caching

`WebImageCache.GetWebImage(url, callback)` returns a cached texture immediately. If a request is already in progress, callbacks join that request instead of starting duplicates. Failed downloads are not cached. Each `WebBillboard` also exposes `GetWebImage(Action<Texture2D>)` for the assignment's one-argument interface. The cache owns textures; billboards own their material instances. The panel reports requests, shared requests/cache hits, and stored textures.

After all six billboards load, expect **3 requests, 3 shared, 3 stored**. Click Retry images to demonstrate reuse: requests remain 3, shared increases by 6. Images retain their aspect ratios.

Three different image server hostnames are used:

- `eol.jsc.nasa.gov`: NASA/Apollo 17, The Blue Marble. [Image source](https://eol.jsc.nasa.gov/Collections/EarthFromSpace/images.pl?photo=AS17-148-22727), [public-domain policy](https://eol.jsc.nasa.gov/Collections/EarthFromSpace/faq.htm). The original Wikimedia server returned HTTP 429; the scene now downloads NASA's 540-pixel copy.
- `nssdc.gsfc.nasa.gov`: NASA/Apollo 16, North America. [Gallery](https://nssdc.gsfc.nasa.gov/photo_gallery/photogallery-earth.html), [public-domain policy](https://nssdc.gsfc.nasa.gov/photo_gallery/photogallery-faq.html).
- `images-assets.nasa.gov`: NASA/JPL Voyager 2, Neptune, PIA01492. [Image and credit](https://images.nasa.gov/details/PIA01492), [NASA media usage policy](https://www.nasa.gov/nasa-brand-center/images-and-media/). This NASA/JPL scientific image is used as US government public-domain imagery, without endorsement claims.

## PlayFab

`PlayFabConnection` implements the Client REST functions RegisterPlayFabUser and LoginWithPlayFab. There is no SDK dependency and no developer secret key in the client. The scene has Title ID `1D58A0` preconfigured. Choose a separate test-player username and password and click Register test player. Later use Log in test player with those player credentials. These are not your PlayFab developer account credentials. The response displays the player ID; verify that player in Game Manager. Passwords are held only for the request and cleared from the UI after submission.

Create your title using [Game Manager](https://developer.playfab.com/) and the [official quickstart](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/gamemanager/quickstart). Do not choose paid services or enter payment details. Registration/login cannot be verified until a title is available. Essam's course tutorial was not supplied, so its exact integration steps have not been checked.

## Verification and submission

Validation on November 22, 2026: Unity batch compilation and the eight sky-state/scene-wiring checks passed (`Logs/WeatherLabValidation.log`). All three image URLs returned HTTP 200 with image/jpeg. The supplied OpenWeather key returned HTTP 401. A headless play-mode cache check stalled, so end-to-end play-mode behavior is not yet verified. PlayFab authentication remains unverified. The Fantasy pack has since been imported and connected.

`Weather Lab > Validate scene and weather mapping` checks all eight sky states, the sun reference, skybox references, six billboards, and the HTTP setting. `Weather Lab > Create scene` regenerates the starter scene and replaces its configuration; it is not needed for normal use.

Before submission: demonstrate a successful live weather response, register/log in a PlayFab test player, and compare the PlayFab function with Essam's tutorial. Capture city changes, weather/time/temperature, the six loaded billboards with cache counters, and PlayFab success. Never include the local credential file in a submission archive. The original SampleScene remains available.

API references: [OpenWeather Current Weather](https://openweathermap.org/current), [PlayFab registration](https://learn.microsoft.com/en-us/rest/api/playfab/client/authentication/register-playfab-user), [PlayFab login](https://learn.microsoft.com/en-us/rest/api/playfab/client/authentication/login-with-playfab).

Billboard fixes (November 23): replaced the rate-limited first image URL in both the scene and generator, kept failed-image status visible when other downloads succeed, guarded image decoding and invalid URLs, cleared interrupted in-flight entries, and prevented older callbacks overwriting newer billboard loads. Runtime and editor scripts compile with Unity''s compiler. Ten deterministic checks against the actual cache source pass using a simulated Unity/network layer; this does not replace visual play-mode verification.

Live weather recheck (November 23): the configured OpenWeather key now returns successful current-weather responses for Orlando, London, Dubai, Tokyo, and Sydney. The earlier HTTP 401 is resolved. PlayFab Title ID is configured, but registration/login still needs a test-player verification.

