#!/bin/bash

WORKSPACE=$1
DRIVE=$2
IS_DEV_BUILD=$3
BUILD="none"

echo IS_DEV_BUILD\($IS_DEV_BUILD\)

if "$IS_DEV_BUILD"; then
	BUILD="dev"
else
	BUILD="build"
fi

echo WORKSPACE\($WORKSPACE\) IS_DEV_BUILD\($IS_DEV_BUILD\) BUILD\($BUILD\)

echo "Start to \"${BUILD}\" script... (package.json)"

cd $DRIVE
cd "${WORKSPACE}"
npm run $BUILD

read -p "Press return/enter key..."
