source ../CONFIG.inc

PACKAGE=KerbalJointReinforcement
TARGETDIR="$PACKAGE"
TARGETBINDIR="$TARGETDIR/Plugins"
PROJECTSDIR="$PACKAGE/"
PLUGINS="$PACKAGE"

VERSION=$( cat $PACKAGE.version | tr '\n' ' ' | sed -n -E 's/^.*?"VERSION\":\{.*"MAJOR":([0-9]+?),.*?"MINOR":([0-9]+?),.*?"PATCH":([0-9]+?),.*?"BUILD":([0-9]+?).*?\}.*$/\1.\2.\3.\4/p' )

if [ ! -f ./bin/KSP.ver ] ; then
    echo "No build."
    exit -1
fi

KSPV=$(cat ./bin/KSP.ver)
