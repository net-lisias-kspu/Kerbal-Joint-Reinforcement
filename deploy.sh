#!/usr/bin/env bash

source ./CONFIG.inc

check() {
	if [ ! -d "./GameData/$TARGETBINDIR/" ] ; then
		rm -f "./GameData/$TARGETBINDIR/"
		mkdir -p "./GameData/$TARGETBINDIR/"
	fi
}

deploy_dev() {
	local DLL=$1

	if [ -f "./bin/Release/$KSPV/$DLL.dll" ] ; then
		cp "./bin/Release/$KSPV/$DLL.dll" "$LIB"
	fi
}

deploy() {
	local DLL=$1

	if [ -f "./bin/Release/$KSPV/$DLL.dll" ] ; then
		cp "./bin/Release/$KSPV/$DLL.dll" "./GameData/$TARGETBINDIR/"
		if [ -d "${KSP_DEV}/GameData/$TARGETBINDIR/" ] ; then
			cp "./bin/Release/$KSPV/$DLL.dll" "${KSP_DEV/}GameData/$TARGETBINDIR/"
		fi
	fi
	if [ -f "./bin/Debug/$KSPV/$DLL.dll" ] ; then
		if [ -d "${KSP_DEV}/GameData/$TARGETBINDIR/" ] ; then
			cp "./bin/Debug/$KSPV/$DLL.dll" "${KSP_DEV}GameData/$TARGETBINDIR/"
		fi
	fi
}

deploy_ver() {
    local KSPA=(${KSPV//\./ })
    local data=$(cat $VERSIONFILE)
    data=${data//\$\{KSPV\}/$KSPV}
    data=${data//\$\{KSPA\[0\]\}/${KSPA[0]}}
    data=${data//\$\{KSPA\[1]\}/${KSPA[1]}}
    if [ '4' == ${KSPA[1]} ] ; then
	data=${data//\$\{KSPA\[2]\}/5}
    else
	data=${data//\$\{KSPA\[2]\}/3}
    fi
    rm "./GameData/$TARGETDIR/*.version"
    echo "${data}" > "./GameData/$TARGETDIR/$VERSIONFILE"
    echo "${data}" > "./$PACKAGE-$KSPV.version"
}


VERSIONFILE=$PACKAGE.version

check
#deploy_ver
cp $VERSIONFILE "./GameData/$TARGETDIR"
cp CHANGE_LOG.md "./GameData/$TARGETDIR"
cp README.md  "./GameData/$TARGETDIR"
cp LICENSE "./GameData/$TARGETDIR"
cp NOTICE "./GameData/$TARGETDIR"

for dll in $PLUGINS ; do
    deploy_dev $dll
    deploy $dll
done
