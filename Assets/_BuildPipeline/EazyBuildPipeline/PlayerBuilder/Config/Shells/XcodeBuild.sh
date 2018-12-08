# #!/bin/bash

#参数判断
if [ $# != 2 ];then
    echo "Params error!"
    echo "Need two params: 1.path of project 2.name of ipa file"
    exit
elif [ ! -d $1 ];then
    echo "The first param is not a dictionary."
    exit

fi
#工程路径
project_path=$1

#IPA名称
ipa_name=$2

#build文件夹路径
build_path=${project_path}/build

#进入Xcode工程目录
cd $project_path

#删除build目录
rm -rf build

#清理#
xcodebuild clean

#编译工程
xcodebuild || exit

#打包#
xcrun -sdk iphoneos PackageApplication -v ${build_path}/*.app -o ${build_path}/${ipa_name}.ipa