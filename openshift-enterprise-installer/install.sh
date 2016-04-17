#region Copyright ©2016, Click2Cloud Inc. - All Rights Reserved
#* -------------------------------------------------------------------- *
#*                            Click2Cloud Inc.                          *
#*                  Copyright ©2016 - All Rights reserved               *
#*                                                                      *
#*                                                                      *
#*  Copyright © 2016 by Click2Cloud Inc. | www.click2cloud.net          *
#*  All rights reserved. No part of this publication may be reproduced, *
#*  stored in a retrieval system or transmitted, in any form or by any  *
#*  means, photocopying, recording or otherwise, without prior written  *
#*  consent of Click2cloud Inc.                                         *
#*                                                                      *
#*                                                                      *
#* -------------------------------------------------------------------- *
#endregion Copyright ©2016, Click2Cloud Inc. - All Rights Reserved
#
chk_subscription()
{

        ####################################################################
        ### Get  Details from user whether RHN User is created or not     ###
        ####################################################################
        clear
        echo -e -n "Do you have RHN Acoount y/n :"
        read isrhn
        if [ $isrhn == 'n' -o $isrhn == 'N' ]
        then
                echo "Kindly Get your subscrition First at https://idp.redhat.com/idp/"
                echo 'Run The Script Again'
                exit
        fi
}
get_rhn_details()
{
        ####################################################################
        ### Accepting RHN User Name and Password ###
        ####################################################################
        clear
        echo '              Subscription Details '
        echo '********************************************'
        read -p  "Enter RHN User Name : " rhn_user
        echo -e -n  "RHN Password : "
        read -s rhn_pass
        echo
        echo "Enter Your Pool_id or Paster here"
        read pool_id
}
get_passwords_broker()
{
        ###################################################################
        # For Broker:           Supplying Password or not                 #
        ###################################################################
        clear
        echo 'Broker:          Password Details '
        echo '********************************************'
        echo -e -n 'Do you want to enter MCollective Password ( y/n): '
        read ismcpass
        echo -e -n 'Do you want to enter Mongodb Broker Password ( y/n): '
        read ismdbpass
        echo -e -n 'Do you want to enter Openshift Password ( y/n): '
        read isospass
        #################################################################
        #               Accepting Passwords if opted                    #
        #################################################################
        if [ $ismcpass == 'y' -o $ismcpass == 'Y' ]
        then
                echo  "Enter MCollective Password: "
                read -s mcpass
                passwords="mcollective_password=$mcpass"
        fi
        if [ $ismdbpass == 'y' -o $ismdbpass == 'Y' ]
        then
                echo  "Enter Mongodb Password: "
                read -s mdbpass
                passwords="$passwords mongodb_broker_password=$mdbpass"
        fi

        if [ $isospass == 'y' -o $isospass == 'Y' ]
        then
                echo  "Enter OpenShift Password: "
                read -s ospass
                passwords="$passwords openshift_password=$ospass"
        fi
}

get_host_domain_broker()
{
        clear
        echo '              Get Host Domain Name  Details '
        echo '********************************************'
        echo "Enter Domain Name (default: apps.example.com) : "
        read domain
        if [ -z "$domain" ]
        then
                domain=apps.example.com
        fi
        echo "Enter hosts domain (default: openshift.example.com) : "
        read hosts_domain
        if [ -z "$hosts_domain" ]
        then
                hosts_domain=openshift.example.com
        fi
        echo "Enter Broker hostname  (default: broker.openshift.example.com) : "
        read broker_hostname
        if [ -z "$broker_hostname" ]
        then
                broker_hostname=broker.openshift.example.com
        fi
}
get_named_entries_broker()
{
        clear
        echo '              Get Name Entries  '
        echo '********************************************'
        echo "Enter Broker ip  (Ex: 192.168.1.201): "
        read broker_ip
        named_entries=`echo $broker_hostname|cut -d"." -f1`:$broker_ip,activemq:$broker_ip
}
get_gear_details_broker()
{
        gear_size=0
        while true
        do
                clear
                echo '              Get Gear Details '
                echo '********************************************'
        #       echo "Enter Valid Gear Sizes (Ex: medium,small) : "
        #       read valid_gear_sizes
                valid_gear_sizes=large,medium,small
                echo "Enter Default Gear Size : "
                echo '  1. Small'
                echo '  2. Medium'
                echo '  3. Large'
                echo '*******************************************'
                echo '          Enter Your Choice (1-3) : '
                read gear_size
                case $gear_size in
                        1) default_gear_size=small
                           break;;
                        2) default_gear_size=medium
                           break;;
                        3) default_gear_size=large
                           break;;
                        *) echo "Incorrect Gear Size Selected "
                           echo "Press Enter Key to Reselect Gear "
                           read key;;
                esac

        #       echo "Enter Default Gear Capabilities (Ex: medium) : "
        #       read default_gear_capabilities
                default_gear_capabilities=$default_gear_size
        done
}

create_broker()
{
        passwords=
        install_components='broker,named,activemq,datastore'
        echo '*****************************************************************'
        echo '           OpenShift Enterprise Broker Installation'
        echo '*****************************************************************'
        echo 'install_component(broker,named,activemq,datastore): '
        install_components='broker,named,activemq,datastore'
        chk_subscription
        get_rhn_details
        get_passwords_broker
        get_host_domain_broker
        get_named_entries_broker
        get_gear_details_broker
}
create_node()
{

        install_components='node'
        echo '*****************************************************************'
        echo '           OpenShift Enterprise Node Installation'
        echo '*****************************************************************'
        chk_subscription
        get_rhn_details
        get_mcollective_password_node
        get_host_domain_node
        get_node_profile
        get_node_cartriges
}
get_mcollective_password_node()
{

        clear
        echo 'Node MCollective Passwdord:'
        echo '********************************************'
        echo -e -n "Enter Mcollective Password : "
        read -s mcollective_password
}
get_host_domain_node()
{
        clear
        echo 'Node:'
        echo '********************************************'
        echo -e -n "domain (default: apps.example.com) : "
        read domain
        if [ -z "$domain" ]
        then
                domain=apps.example.com
        fi
        echo -e -n "Host Domain (default: openshift.example.com) : "
        read hosts_domain
        if [ -z "$hosts_domain" ]
        then
                hosts_domain=openshift.example.com
        fi
        echo -e -n "Broker Host Name (default: broker.openshift.example.com) : "
        read broker_hostname
        if [ -z "$broker_hostname" ]
        then
                broker_hostname=broker.openshift.example.com
        fi
        echo -e -n "Enter Broker ip address is compulsory (Ex: 192.168.1.201) : "
        read broker_ip_addr
        echo -e -n "Node Host Name (default: linode1.openshift.example.com) : "
        read node_hostname
        if [ -z "$node_hostname" ]
        then
                node_hostname=linode1.openshift.example.com
        fi

}
get_node_profile()
{
        while true
        do
                clear
                echo 'Get Node Profile:'
                echo '********************************************'
                echo '  1. Small'
                echo '  2. Medium'
                echo '  3. Large'
                echo '*******************************************'
                echo '          Enter Your Choice (1-3) : '
                read np
                case $np in
                        1) node_profile=small
                           break;;
                        2) node_profile=medium
                           break;;
                        3) node_profile=large
                           break;;
                        *) echo "Incorrect Gear Profile Selected "
                           echo "Press Enter Key to Reselect"
                           read key;;
                esac
        done
}
get_node_cartriges()
{

        clear
        echo 'Node Cartridges:'
        echo '********************************************'
        echo -e -n "Cartridges (default:  php,ruby,postgresql,haproxy,jenkins) : "
        read cartridges
        if [ -z "$cartridegs" ]
        then
                 cartridges=php,ruby,postgresql,haproxy,jenkins
        fi
}
run_script()
{
        clear
        echo '**************************************************************'
        if [ $choice -eq 1 ]
        then
                echo "sh openshift.sh install_method=rhsm rhn_user=$rhn_user rhn_pass=$rhn_pass sm_reg_pool=$pool_id install_components=broker,named,activemq,datastore $passwords domain=$domain hosts_domain=$hosts_domain broker_hostname=$broker_hostname named_entries=$named_entries valid_gear_sizes=$valid_gear_sizes default_gear_size=$default_gear_size default_gear_capabilities=$default_gear_size 2>&1 | tee -a openshift.sh.broker.log"
        else
                echo "sh openshift.sh install_method=rhsm rhn_user=$rhn_user rhn_pass=$rhn_pass sm_reg_pool=$pool_id install_components=node mcollective_password=$mcollective_password  domain=$domain hosts_domain=$hosts_domain node_hostname=$node_hostname broker_ip_addr=$broker_ip_addr broker_hostname=$broker_hostname node_profile=$node_profile cartridges=$cartridges 2>&1 | tee -a openshift.sh.node.log"
        fi

        echo '**************************************************************'
        echo
        echo 'check all above parameters'
        echo 'if correct then press y otherwise press n'
        read key
        if [ $key == 'Y' -o $key == 'y' ]
        then
                if [ $choice -eq 1 ]
                then
                        sh openshift.sh install_method=rhsm rhn_user=$rhn_user rhn_pass=$rhn_pass sm_reg_pool=$pool_id install_components=broker,named,activemq,datastore $passwords domain=$domain hosts_domain=$hosts_domain broker_hostname=$broker_hostname named_entries=$named_entries valid_gear_sizes=$valid_gear_sizes default_gear_size=$default_gear_size default_gear_capabilities=$default_gear_size 2>&1 | tee -a openshift.sh.broker.log
                else
                        sh openshift.sh install_method=rhsm rhn_user=$rhn_user rhn_pass=$rhn_pass sm_reg_pool=$pool_id install_components=node mcollective_password=$mcollective_password  domain=$domain hosts_domain=$hosts_domain node_hostname=$node_hostname broker_ip_addr=$broker_ip_addr broker_hostname=$broker_hostname node_profile=$node_profile cartridges=$cartridges 2>&1 | tee -a openshift.sh.node.log
                sed -i -e 's/^TRAFFIC_CONTROL_ENABLED.*/TRAFFIC_CONTROL_ENABLED=true/' /etc/openshift/node.conf
                fi
        fi
}

manage_district()
{
        dchoice=0
        while [ $dchoice -ne 3 ]
        do
                clear
                echo -e "\t\tM A  N A G E  D I S T R I C T"
                echo
                echo -e "\t1    Create District"
                echo
                echo -e "\t2    Add Node to a district"
                echo
                echo -e "\t3    Return to  Main Menu"
                echo
                echo -e -n "\tEnter  Your  Choice (1-3) : "
                read dchoice
                case $dchoice in
                        1) create_district;;
                        2) add_node;;
                        3) break;;
                esac
                read -p "Press Enter Key to Continue " key
        done
}

create_district()
{
        while true
        do
                clear
                echo 'District Creation on Broker:'
                echo '****************************'
                echo '           Please Select the platform for district '
                echo
                echo '                  1 Linux'
                echo '                  2 Windows'
                echo -e -n "           Enter Your choice : "
                read pc
                case $pc in
                        1)platform=linux
                          break;;
                        2)platform=windows
                          break;;
                        *)echo 'Incorrect Platform Select'
                          echo 'Press enter key to reselect platform!!!'
                          read key
                          continue;
                esac
        done
        echo -e -n "\tEnter District Name : "
        read district_name
        if [ -z "$district_name" ]
        then
                echo 'District Name Can not be empty!!!!!'
        else

                oo-admin-ctl-district -c create -o $platform -n $district_name
        fi
        echo 'Press Enter Key to continue'
        read key
}

add_node()
{
        clear
        echo 'Adding Node to a District :'
        echo '**************************'
        echo -e -n "\tEnter District Name : "
        read district_name
        if [ -z "$district_name" ]
        then
                echo 'District Name Can not be empty!!!!!'
        else
                echo -e -n "\tEnter Node FQDN Name (Ex. linode1.openshift.example.com) : "
                read node_name
                if [ -z "$node_name" ]
                then
                        echo 'Node Name can not be empty'
                else
                        oo-admin-ctl-district -c add-node -n $district_name -i $node_name
                fi
        fi
}
##################################################################
######### Program execution starts here ############
#################################################################
choice=0
while [ $choice -ne 6 ]
do
        clear
        echo -e "\t\tM A I N   M E N U"
        echo -e '\t\t*****************'
        echo -e "\t1    Install Broker"
        echo
        echo -e "\t2    Install Node"
        echo
        echo -e "\t3    Check Broker"
        echo
        echo -e "\t4    Check Node"
        echo
        echo -e "\t5    Manage District on broker"
        echo
        echo -e "\t6    Exit"
        echo
        echo -e -n "\t  Enter  Your  Choice (1-6) : "
        read choice
        case $choice in
                1) create_broker
                   run_script;;
                2) create_node
                   run_script;;
                3) oo-accept-broker -v;;
                4) oo-accept-node -v;;
                5) manage_district;;
                6) break;;
        esac
        read -p "Press Enter Key to Continue " key
done
