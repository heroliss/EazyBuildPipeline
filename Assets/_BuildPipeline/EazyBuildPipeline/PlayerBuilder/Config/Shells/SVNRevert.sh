#!/bin/bash
targetPath=$1
logPath=$2

echo "Start Revert ${targetPath}" | tee -a ${logPath}
svn --non-interactive revert -R ${targetPath} | tee -a ${logPath}
echo "End Revert" | tee -a ${logPath}