#!/bin/bash

WORKSPACE=$1
DRIVE=$2

echo DRIVE\($DRIVE\) WORKSPACE\($WORKSPACE\)

cd $DRIVE
cd "${WORKSPACE}"

echo "Try to run tsc..."
tsc
