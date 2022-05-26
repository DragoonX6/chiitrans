#!/usr/bin/sh

set -e

BASEDIR=$(realpath $(dirname $0))
COFFEE=${BASEDIR}/Tools/CoffeeSharp/Coffee.exe

cd ${BASEDIR}/ChiitransLite/www/js

for script in *.coffee; do
	${COFFEE} -c $script
done

