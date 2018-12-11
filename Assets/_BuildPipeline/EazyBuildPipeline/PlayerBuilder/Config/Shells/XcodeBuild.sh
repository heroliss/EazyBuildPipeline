# #!/bin/bash
function Proj()
{
    xcodebuild -project $projectName $*
}

#参数判断
if [ $# != 2 ];then
    echo "Params error!"
    echo "Need two params: 1.work path  2.xcode project folder name"
    exit
elif [ ! -d $1 ];then
    echo "The first param is not a dictionary."
    exit
fi

workPath=$1
projectName=$2

#进入工作目录
cd $workPath

#清理#
Proj clean

#构建
Proj archive \
-archivePath "Archive" \
-scheme "Unity-iPhone" \
-configuration "Release" \

#生成ipa
xcodebuild -exportArchive \
-archivePath "Archive.xcarchive" \
-exportPath "IPA" \
-exportOptionsPlist "IPA/ExportOptions.plist"