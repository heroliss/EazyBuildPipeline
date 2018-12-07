# #!/bin/bash

#�����ж�
if [ $# != 2 ];then
    echo "Params error!"
    echo "Need two params: 1.path of project 2.name of ipa file"
    exit
elif [ ! -d $1 ];then
    echo "The first param is not a dictionary."
    exit

fi
#����·��
project_path=$1

#IPA����
ipa_name=$2

#build�ļ���·��
build_path=${project_path}/build

#����Xcode����Ŀ¼
cd $project_path

#ɾ��buildĿ¼
rm -rf build

#����#
xcodebuild clean

#���빤��
xcodebuild || exit

#���#
xcrun -sdk iphoneos PackageApplication -v ${build_path}/*.app -o ${build_path}/${ipa_name}.ipa