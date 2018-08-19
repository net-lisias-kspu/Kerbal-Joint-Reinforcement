# Kerbal Joint Reinforcement /L :: Change Log

## Missing Binaries
v2.2

	Features
	--Updated to function with KSP ARM Patch (KSP 0.23.5)
	--Removed inertia tensor fix, as it is now stock
	--Main stiffening / strengthening is now disabled by default due to stock joint improvements
	--Decoupler stiffening is now disabled by default due to stock joint improvements

	Bugfixes:
	--Vessels can no longer become permanently indestructible

v2.1

	Features
	--Reduced extent of decoupler stiffening joint creation; this should reduce physics overhead
	--Code refactoring for additional performance gains
	--Removed physics easing effect on inertia tensors; was unnecessary and added more overhead
	--Workaround for the stock "Launch Clamps shift on the pad and overstress your ship" bug that is particularly noticeable with RSS
	--Clamp connections are stiffer; now allowed by above workaround

	Bugfixes
	--KAS struts no longer break on load


v2.0

	Features
	--Full release of proper inertia tensors!  Massive parts will feel more massive.
	--Full release of greater physics easing!  Landed and pre-launch crafts will have gravitational, centrifugal and coriolis forces slowly added to them, reducing the initial physics jerk tremendously
	--Launch clamps are now much stiffer when connected to more-massive-than-stock mod parts
	--Tightened up default joint settings more
	--Decoupler Stiffening Extension will now extend one part further if it's final part is much less massive than its parent / child part
	--Added Majiir's CompatibilityChecker; this will simply warn the user if they are not using a compatible version of KSP

	Bugfixes
	--Joints during physics easing strengthened

v2.0x2

	Features
	--Elaborated physics easing: joints' flexion range is initially great and decreases, and gravitational + rotating ref frame forces are cancelled out to resolve internal joint stresses ere loading the rocket
	--Greatly tightened default joint settings

	Bugfixes
	--Non-zero angular limits no longer wrongly reorient parts.


v2.0x1

	Features
	--Fixed part inertia tensors: heavy, large objects should now "feel" more massive, and their connections should better behave. Thanks to a.g. for finding this one.
	--Slightly stiffened Launch Clamps
	--Removed v1.7's improper stiffening for stretchy tanks, which the ability to stretch stretchy tanks makes unnecessary

	Bugfixes
	--Non-zero angular limits no longer wrongly reorient parts.


v1.7

	Features
	--Connection area can be from volume instead of connection area calculated; for very, very large vehicles that the standard settings cannot handle
	--Default joint parameters stiffened
	--Stretchy tanks stiffened--a better solution is being developed while this one helps RSS

	Bugfixes
	--Decoupling no longer further stiffens joints being deleted from non-staged decouplers during decoupling / partial crashing


v1.6

	Features
	--BreakStrengthPerUnitArea will not override large breakForces, easing I-beams and structural elements' use

	Bugfixes
	--Fixed decoupler-dockingport combination parts from causing strange disassembly when undocking


v1.5

	Features
	--Updated to KSP 0.23
	--Joint breaking strength can be set to increase with connection area so that large part connections can have realistically large strength; on by default
	--Vessels are further strengthened for the first 30 physics frames after coming off rails or loading, reducing initialization jitters.

	Bugfixes:
	--Launch clamps after staging remain clamped to the ground.
	--Kraken no longer throws launchpads at orbiting craft


v1.4.2

	Bugfixes
	--Wobble reduced
	--General tweaks to reduce wobbling further


v1.4.1

	Bugfixes
	--Maximum joint forces correctly calculated
	--Docking no longer causes exceptions to be thrown and cause lag


v1.4

	Features
	--Increased calculation of surface-attached connection area's accuracy

	Bugfixes
	--Wobble between stack-attached parts of very different sizes greatly reduced


v1.3

	Features
	--Better solution for failure to apply decoupler ejection forces
	--Will not stiffen parts below a given mass, which can be changed in config
	--Properly updates on docking

	Bugfixes
	--Launch clamps no longer to the surface lock ships


v1.2

	Features
	--Workaround for stock KSP bug where struts would prevent decoupler ejection forces from being applied

	Tweaks
	--Reduced default maxForceFactors to be more reasonable levels

	BugFixes
	--Struts properly disconnect
	--Decouplers properly function


v1.1

	Features
	--Stiffness of joint no longer erroneously dependent on breakForce / breakTorque
	--Decoupler stiffening function made more comprehensive

	BugFixes
	--Further decoupler stiffening affects radial decouplers
	--Decoupler further stiffening no longer causes Nulls to be thrown when attached to physics-disabled parts
	--Procedural fairings no longer locked to rockets
	--Infernal Robotics parts function
	--Temporary stopgap measure: stiffening not applied to pWings to prevent ultra-flexy wings

	Known Issues
	--Decouplers exert no detach force with extra decoupler stiffening enabled
	Same issues as strut attachment bug


v1.0

	Release
