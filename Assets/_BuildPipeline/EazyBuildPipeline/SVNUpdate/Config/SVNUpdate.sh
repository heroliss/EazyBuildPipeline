#!/bin/bash
logPath=$1

svn --non-interactive revert -R . | tee -a ${logPath}
svn --non-interactive update | tee -a ${logPath}
svn --non-interactive resolve --accept theirs-full -R | tee -a ${logPath}

echo "SVN Revert & Update & Resolve All Finished!" | tee -a ${logPath}