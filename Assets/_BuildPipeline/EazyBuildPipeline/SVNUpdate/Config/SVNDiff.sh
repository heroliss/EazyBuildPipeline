#!/bin/bash

svn diff --diff-cmd "diff" -x "-q" . | grep Index | cut -d " " -f 2
