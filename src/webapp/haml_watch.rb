# Script to watch a directory for any changes to a haml file
# and compile it.
#
# USAGE: ruby haml_watch.rb <directory_to_watch>
#  
require 'rubygems'
require 'fssm'

#source_dir = Dir.pwd + "/" + ARGV.first
#source_dir = ARGV.first

#source_dir = Dir.pwd + "/haml"
source_dir = Dir.pwd
#dest_dir = Dir.pwd + "/webapp"
dest_dir = Dir.pwd

puts "Monitoring " + source_dir
puts "Target Dir: " + dest_dir

FSSM.monitor(source_dir, '**/*.haml') do
  update do |base, relative|
    input = source_dir + "/" + "#{relative}"
    output = dest_dir + "/" + "#{relative.gsub!('.haml', '.html')}"
    command = "haml #{input} #{output}"
    %x{#{command}}
    puts "Regenerated #{input} to #{output}"
  end
end

