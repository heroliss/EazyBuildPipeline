#/bin/bash
srcPath=$1
targetPath=$2

folderPath=`dirname ${targetPath}`
mkdir -p ${folderPath}
curl -o ${targetPath} ${srcPath}