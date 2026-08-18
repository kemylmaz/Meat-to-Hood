# Poly Pizza source bundles

The models under this folder are a curated subset of eight bundles downloaded from
[poly.pizza](https://poly.pizza). `PolyPackBuilder` imports them into
`Assets/ShawarmaTycoon/Resources/PolyPrefabs`; nothing here is referenced by the
game directly.

| Folder       | Bundle                     | Source |
| ------------ | -------------------------- | ------ |
| `City`       | City Builder Bits          | https://poly.pizza/bundle/City-Builder-Bits-1wLdnIddSx |
| `People`     | Animated Men Pack          | https://poly.pizza/bundle/Animated-Men-Pack-DAC9SDgMQT |
| `People`     | Animated Women Pack        | https://poly.pizza/bundle/Animated-Women-Pack-HHSKxnk1mY |
| `Restaurant` | Restaurant Bits            | https://poly.pizza/bundle/Restaurant-Bits-ejkcnWf78Q |
| `Kitchen`    | Charming Kitchen set       | https://poly.pizza/bundle/Charming-Kitchen-set-DsYQKb1K4M |
| `Interior`   | Ultimate Interior Props    | https://poly.pizza/bundle/Ultimate-Interior-Props-Pack-9KfkK2H0ve |
| `Food`       | Food Kit                   | https://poly.pizza/bundle/Food-Kit-vOc58LJ0ge |
| —            | Cooking Assets (not used)  | https://poly.pizza/bundle/Cooking-Assets-FKGoA2lmGL |

**Before shipping a build, confirm each bundle's licence on its page above and add
the required author credit.** Poly Pizza carries both CC0 and CC-BY work, and the
CC-BY bundles have to name their author somewhere the player can reach. The
downloads do not include a licence file, so that information only exists on the
bundle pages.

## Notes

- `City`, `Restaurant` and `Kitchen` each carry one palette atlas embedded in
  their FBX files. `PolyPackBuilder` unpacks those into a `Textures` subfolder and
  point-filters them; without that step the models import plain white.
- `Interior` and `People` have no textures - their colour is in the material slots.
- `Food` is OBJ + MTL, with flat diffuse colours and no texture at all.
- The bundles were authored at four unrelated scales. Every model states one real
  measurement in `PolyPackBuilder.Specs` and the builder solves for the rest.
- The Charming Kitchen set carries the shop's shell as well as its fittings: the
  wall, window and doorway panels and the floor tile are a modular kit on a
  2-unit grid, scaled to a 1.41 m pitch by `ShopWorldBuilder.ShellModule`. Only
  the panels' +Z face is finished, so a run has to be turned to face inward.
