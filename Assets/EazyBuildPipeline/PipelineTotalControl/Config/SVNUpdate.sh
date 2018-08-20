#/bin/bash

SVN="svn --non-interactive"
${SVN} revert -R .
${SVN} update
#${SVN} resolve --accept theirs-conflict -R
