#!/usr/bin/env bash

echo "Need maintainance! Implement KSPV!"
exit -1

source ./CONFIG.inc

VERSIONFILE=$PACKAGE.version

scp -i $SSH_ID ./GameData/net.lisias.ksp/$VERSIONFILE $SITE:/$TARGETPATH