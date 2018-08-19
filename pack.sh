#!/usr/bin/env bash

source ./CONFIG.inc

clean() {
	rm $FILE
	if [ ! -d Archive ] ; then
		rm -f Archive
		mkdir Archive
	fi
}

KSP=$(git rev-parse --abbrev-ref HEAD)
KSP=${KSP/\//_}

FILE=$PACKAGE-$VERSION-$KSP.zip
echo $FILE
clean
zip -r $FILE ./GameData/* -x ".*"
zip -r $FILE ./PluginData/* -x ".*"
zip -d $FILE __MACOSX "**/.DS_Store"
mv $FILE ./Archive
