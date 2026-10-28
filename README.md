# Unity Bullet Effects

A basic bullet tracer system for Unity URP, built for first and third person shooters. This asset aims to provide free visual effects for different weapons that are not hard to implement and provide a solid end result.

Tracers are harder to get right than they look. Fire at a wall three metres away and a bullet at real speed crosses the gap in less than a single frame, so the tracer never renders at all. Fire at the sky and a line a few centimetres wide becomes thinner than a pixel long before it reaches anything, so it disappears mid-flight. Many varieties of tracers end up as static beams because of these two problems.

This package handles both. Tracers are guaranteed several frames of visible travel at any distance, and hold a minimum on-screen thickness so long shots stay readable all the way out. The result reads as a round in flight rather than a line that appears and vanishes without seeming to visually move.

Key features
Visible at any range. Tracers never drop below a set on-screen thickness, so distant shots stay readable instead of vanishing into sub-pixel lines.
No frame skipping at close range. Short shots are given enough travel time to render across several frames, so they still should read as moving. This has been tested at 60 frames, 120 frames, 144 frames, 180 frames and uncapped frames.
Automatic close-range sizing. Tracers shorten and thin as the target gets nearer, and stop spawning entirely at point-blank.
Long-range linger. Shots into open sky stay visible for seconds rather than snapping to their endpoint and disappearing immediately.
Per-shot spawn variation. Rapid fire reads as separate rounds instead of one solid beam.
Object pooling built in. No garbage allocation during sustained fire.
Works with any weapon script. Two method calls. No assumptions about how you handle shooting, weapons or input.
Both input systems supported. Old Input Manager and the Input System package, with no setup either way.
What is included
Bullet tracer component and prefab
Smoke trail that travels to the impact point and collides with geometry
Muzzle flash, muzzle sparks and muzzle smoke particle systems
Three tracer texture variants
Playable demo scene with runtime toggles for each effect
Example weapon, movement and mouse look scripts
User guide
Customisation
Every part is adjustable from the inspector. The tracer exposes length, travel speed, lifetime, close-range width falloff, long-range linger speed, minimum on-screen thickness and spawn variation, each with a tooltip explaining what it does.
Three tracer textures are included, and swapping in your own is a matter of dropping it into the material. The muzzle effects are standard Unity particle systems, so they can be retuned or replaced with your own.
The user guide covers every setting, how the pieces fit together, and how to build a tracer prefab from scratch if you want to understand the setup rather than use the included one.

Suitable for
First and third person shooters, top-down shooters, and anything else that fires a projectile. Works with hitscan weapons out of the box. The tracer takes a start point and an end point, so it suits any weapon system that can tell it where the shot landed.

Requirements
Unity 6 or newer
Universal Render Pipeline
No external dependencies, and no additional packages required
Notes
The included shooter, movement and mouse look scripts exist so the demo scene is playable. They are examples rather than the asset itself, and can be deleted.
Built and tested in Unity 6 (6000.4.10f1) with URP. Not tested in older releases, the Built-in Render Pipeline, or HDRP.
All features can be tuned, so if a long-range linger is not desired, with value changes this can be removed, as with most other visual effects in this asset.
This asset is tested with a very simple hitscan shooting logic, but the tracers can be made to look even better visually with more sophisticated, realistic bullet ballistics.
