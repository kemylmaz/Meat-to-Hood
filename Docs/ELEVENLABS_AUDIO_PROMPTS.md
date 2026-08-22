# Meat & Eat — ElevenLabs Audio Pack

The game now automatically prefers authored audio placed at the paths below and
falls back to its procedural sound effects when a file is missing. Export WAV,
44.1 or 48 kHz, without normalization clipping. Do not include spoken words.

## Music

Target: `Assets/ShawarmaTycoon/Resources/Audio/Music/meat_and_eat_main_loop.wav`

Prompt:

> Seamlessly loopable cozy cartoon restaurant tycoon music, 96 BPM, cheerful and gently busy, warm nylon-string guitar, pizzicato strings, soft marimba, brushed hand percussion, subtle oud and qanun accents for a welcoming Turkish and Eastern Mediterranean flavor. Light playful melody, satisfying management-game rhythm, friendly family atmosphere, no vocals, no dramatic cinematic build, no heavy bass, no sharp cymbals. Keep the arrangement uncluttered so repeated gameplay sound effects remain clear. Exact clean loop, 90 seconds, ending must connect naturally to the opening beat, no fade-out.

## Sound effects

Generate each cue as a separate, isolated sound. No background music, voices,
room tone, long reverb tail, or silence at the beginning.

### `pickup.wav`

> Cozy cartoon item pickup, a soft wooden pop followed by a tiny bright upward pluck, tactile and friendly, 0.20 seconds, clean isolated one-shot, no reverb, no voice, suitable for frequent repetition in a mobile restaurant game.

### `drop.wav`

> Cozy cartoon food handoff and tray placement, gentle ceramic tap with a warm soft thunk and a very small success sparkle, 0.25 seconds, clean isolated one-shot, no reverb, no voice, never harsh.

### `cook.wav`

> Short appetizing shawarma cooking cue, quick warm grill sizzle with a playful rising wooden tick, 0.45 seconds, cozy stylized cartoon sound, clean isolated one-shot, no fire roar, no music, no voice.

### `coin.wav`

> Satisfying cartoon coin pickup, two tiny warm brass chimes in a quick ascending pattern, premium but cute, 0.30 seconds, clean isolated one-shot, no casino feeling, no reverb tail, no voice.

### `cash_register.wav`

> Cozy restaurant cash register sale, soft mechanical drawer click, short paper receipt flick, then one warm coin chime, 0.55 seconds, polished cartoon mobile-game sound, clean isolated one-shot, no voice, no loud bell.

### `combo_up.wav`

> Playful combo increase cue, two quick rounded marimba notes rising in pitch with a tiny sparkle at the end, energetic but soft, 0.35 seconds, clean isolated one-shot, no voice, no long reverb.

### `error.wav`

> Friendly cartoon cannot-do-that cue, muted wooden boop descending in pitch, informative rather than punishing, 0.28 seconds, clean isolated one-shot, no buzzer harshness, no voice, no reverb.

### `unlock.wav`

> Cozy restaurant equipment unlock, three warm plucked notes rising into a short magical sparkle, exciting and welcoming, 0.75 seconds, clean isolated one-shot, no voice, no huge cinematic impact, short controlled tail.

### `reward.wav`

> Charming tycoon milestone reward, four bright warm notes climbing into a soft celebratory shimmer, satisfying and premium, 1.0 second, clean isolated one-shot, no fanfare brass, no voice, controlled reverb tail.

### `trash.wav`

> Cute restaurant cleanup cue, light paper-and-plate rustle followed by a soft bin lid thunk, hygienic and satisfying rather than gross, 0.45 seconds, clean isolated one-shot, no breaking glass, no voice, no room ambience.

### `customer_arrive.wav`

> Friendly new customer arrival notification, one soft wooden knock and a tiny welcoming bell pluck, upbeat but unobtrusive, 0.35 seconds, clean isolated one-shot, no door ambience, no footsteps, no voice.

## Import notes

- Keep music stereo; mono or narrow stereo both work well for sound effects.
- Enable **Loop** on the music clip import settings if Unity does not detect it.
- Leave the exact filenames unchanged. The runtime loads these paths automatically.
- If a generated effect feels tiring after ten repetitions, shorten its tail before lowering its pitch; the cue should remain readable on phone speakers.
