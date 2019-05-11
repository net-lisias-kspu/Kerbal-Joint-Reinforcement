# Kerbal Joint Reinforcement /L Experimental :: Changes

* 2018-1229: 3.4.0.4 (lisias) for KSP >= 1.2
	+ Adding support for ConfigNode (CFG) file format for the *user serviceable settings file* (`user.cfg`) on `<KSP_ROOT>/PluginData/KerbalJointReinforcement`
	+ Lifting the Max KSP restriction on `.version` file.
	+ This release **demands** the [newest KSPe](https://github.com/net-lisias-ksp/KSPAPIExtensions/releases), or things will not work. 
* 2018-1206: 3.4.0.3 (lisias) for {1.2 <= KSP <= 1.5.1}
	+ Splitting configuration files between **stock** and **user customizable** files.
	+ Some love to Logging
		- You will be flooded by log messages on debug mode! 
	+ Adding a INSTALL.md file with proper install instructions
* 2018-1202: 3.4.0.2 (lisias) for KSP 1.2 & 1.3 & 1.4 & 1.5
	+ Logging `config.xml` status when loading.
	+ REMERGE from ferrram4's and meiru's changes
		- Previous merge was trashed.
		- This is a new code-tree
	+ Bumping up version to match meiru's codetree 
	+ (Really now) Preventing KJR to mess with [Global/Ground Construction](https://forum.kerbalspaceprogram.com/index.php?/topic/50911-13-kerbal-joint-reinforcement-v333-72417/&do=findComment&comment=3497716).
		- Fixed as instructed on [Critter79606](https://forum.kerbalspaceprogram.com/index.php?/topic/50911-13-kerbal-joint-reinforcement-v333-72417/&do=findComment&comment=3494635) :)
	+ Preventing KJR to mess with [DockRotate](https://forum.kerbalspaceprogram.com/index.php?/topic/170484-15-14-dockrotate-lightweight-robotics-rotational-control-on-docking-ports-plus-noderotate-make-any-part-rotate/).
		- Change from [peteletroll](https://forum.kerbalspaceprogram.com/index.php?/profile/144573-peteletroll/), also mentioned by [AccidentalDisassembly](https://forum.kerbalspaceprogram.com/index.php?/topic/171377-130l-145-grounded-modular-vehicles-r40l-new-light-texture-switch-alternatives-fixes-oct-9-2018/&do=findComment&comment=3316608)
* 2018-1127: 3.3.3.4 (lisias) for KSP 1.2 & 1.3 & 1.4 & 1.5
	+ Tested (almost properly) on KSP 1.2 :)
		- 'Unifying' the releases in a single distribution file. 
	+ Fixed a typo on the configuration file
		- Thanks, [Critter79606](https://forum.kerbalspaceprogram.com/index.php?/topic/50911-13-kerbal-joint-reinforcement-v333-72417/&do=findComment&comment=3494635)
	+ Preventing KJR to mess with [Global/Ground Construction](https://forum.kerbalspaceprogram.com/index.php?/topic/154167-145-global-construction/).
		- Thanks again, , [Critter79606](https://forum.kerbalspaceprogram.com/index.php?/topic/50911-13-kerbal-joint-reinforcement-v333-72417/&do=findComment&comment=3494635) :)
	+ Using KSPe Logging Facilities
		- I expect that errors on the LoadConstant method do not pass through silently again.  
